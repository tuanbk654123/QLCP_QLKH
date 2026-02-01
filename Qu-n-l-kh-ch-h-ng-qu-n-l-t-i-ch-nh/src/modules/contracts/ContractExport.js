import { Document, Packer, Paragraph, TextRun, AlignmentType, HeadingLevel } from 'docx';
import { saveAs } from 'file-saver';
import moment from 'moment';

// Helper function để format currency
const formatCurrency = (amount) => {
  if (!amount) return '';
  return new Intl.NumberFormat('vi-VN', {
    style: 'currency',
    currency: 'VND',
  }).format(amount);
};

// Helper function để format date
const formatDate = (date) => {
  if (!date) return '';
  return moment(date).format('DD/MM/YYYY');
};

export const exportContractToWord = async (contract) => {
  if (!contract) return;

  const renderTerms = () => {
    if (!contract.terms) return [];
    let terms = [];
    if (Array.isArray(contract.terms)) {
      terms = contract.terms;
    } else if (typeof contract.terms === 'string') {
      terms = contract.terms.split('\n').filter(t => t.trim());
    }
    return terms.map((term, index) => 
      new Paragraph({
        children: [
          new TextRun({
            text: `${index + 1}. ${term}`,
            font: 'Times New Roman',
            size: 22,
          }),
        ],
        spacing: { after: 200 },
      })
    );
  };

  const doc = new Document({
    sections: [
      {
        properties: {},
        children: [
          // Header
          new Paragraph({
            text: 'CỘNG HÒA XÃ HỘI CHỦ NGHĨA VIỆT NAM',
            heading: HeadingLevel.HEADING_1,
            alignment: AlignmentType.CENTER,
            spacing: { after: 200 },
          }),
          new Paragraph({
            text: 'Độc lập - Tự do - Hạnh phúc',
            alignment: AlignmentType.CENTER,
            spacing: { after: 400 },
          }),
          new Paragraph({
            text: '━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━',
            alignment: AlignmentType.CENTER,
            spacing: { after: 400 },
          }),
          new Paragraph({
            text: contract.contractType?.toUpperCase() || 'HỢP ĐỒNG',
            heading: HeadingLevel.HEADING_1,
            alignment: AlignmentType.CENTER,
            spacing: { after: 200 },
          }),
          new Paragraph({
            text: `Số: ${contract.contractNumber || 'HD-2024-XXX'}`,
            alignment: AlignmentType.CENTER,
            spacing: { after: 600 },
          }),

          // Body
          new Paragraph({
            text: `Hôm nay, ngày ${formatDate(contract.contractDate) || '__/__/____'}`,
            alignment: AlignmentType.CENTER,
            spacing: { after: 400 },
          }),

          new Paragraph({
            text: 'BÊN A (Bên thuê/Bên mua):',
            heading: HeadingLevel.HEADING_2,
            spacing: { after: 200 },
          }),
          new Paragraph({
            text: `Tên: ${contract.partyA?.name || '_________________'}`,
            spacing: { after: 200 },
          }),
          ...(contract.partyA?.taxCode ? [new Paragraph({
            text: `Mã số thuế: ${contract.partyA.taxCode}`,
            spacing: { after: 200 },
          })] : []),
          ...(contract.partyA?.address ? [new Paragraph({
            text: `Địa chỉ: ${contract.partyA.address}`,
            spacing: { after: 200 },
          })] : []),
          ...(contract.partyA?.representative ? [new Paragraph({
            text: `Người đại diện: ${contract.partyA.representative}`,
            spacing: { after: 200 },
          })] : []),
          ...(contract.partyA?.position ? [new Paragraph({
            text: `Chức vụ: ${contract.partyA.position}`,
            spacing: { after: 200 },
          })] : []),
          ...(contract.partyA?.phone ? [new Paragraph({
            text: `Điện thoại: ${contract.partyA.phone}`,
            spacing: { after: 200 },
          })] : []),
          ...(contract.partyA?.email ? [new Paragraph({
            text: `Email: ${contract.partyA.email}`,
            spacing: { after: 400 },
          })] : []),

          new Paragraph({
            text: 'BÊN B (Bên được thuê/Bên bán):',
            heading: HeadingLevel.HEADING_2,
            spacing: { after: 200 },
          }),
          new Paragraph({
            text: `Tên: ${contract.partyB?.name || '_________________'}`,
            spacing: { after: 200 },
          }),
          ...(contract.partyB?.idCard ? [new Paragraph({
            text: `CMND/CCCD/Mã số thuế: ${contract.partyB.idCard}`,
            spacing: { after: 200 },
          })] : []),
          ...(contract.partyB?.address ? [new Paragraph({
            text: `Địa chỉ: ${contract.partyB.address}`,
            spacing: { after: 200 },
          })] : []),
          ...(contract.partyB?.position ? [new Paragraph({
            text: `Chức vụ/Vị trí: ${contract.partyB.position}`,
            spacing: { after: 200 },
          })] : []),
          ...(contract.partyB?.phone ? [new Paragraph({
            text: `Điện thoại: ${contract.partyB.phone}`,
            spacing: { after: 200 },
          })] : []),
          ...(contract.partyB?.email ? [new Paragraph({
            text: `Email: ${contract.partyB.email}`,
            spacing: { after: 400 },
          })] : []),

          new Paragraph({
            text: 'Hai bên thỏa thuận ký kết hợp đồng với các điều khoản sau:',
            spacing: { after: 400 },
          }),

          new Paragraph({
            text: 'ĐIỀU 1: ĐỐI TƯỢNG HỢP ĐỒNG',
            heading: HeadingLevel.HEADING_2,
            spacing: { after: 200 },
          }),
          ...(contract.contractType === 'Hợp đồng lao động' ? [
            new Paragraph({
              text: `Bên A thuê Bên B làm việc tại ${contract.partyA?.name || 'công ty'} với vị trí ${contract.partyB?.position || '_________________'}.`,
              spacing: { after: 200 },
            }),
            ...(contract.jobDescription ? [new Paragraph({
              text: `Mô tả công việc: ${contract.jobDescription}`,
              spacing: { after: 200 },
            })] : []),
            ...(contract.workingHours ? [new Paragraph({
              text: `Thời gian làm việc: ${contract.workingHours}`,
              spacing: { after: 200 },
            })] : []),
            ...(contract.salary ? [new Paragraph({
              text: `Mức lương: ${formatCurrency(contract.salary)}/tháng`,
              spacing: { after: 200 },
            })] : []),
          ] : []),
          ...(contract.contractType === 'Hợp đồng dịch vụ' ? [
            new Paragraph({
              text: 'Bên B cung cấp dịch vụ cho Bên A với nội dung:',
              spacing: { after: 200 },
            }),
            ...(contract.serviceDescription ? [new Paragraph({
              text: contract.serviceDescription,
              spacing: { after: 200 },
            })] : []),
            ...(contract.serviceFee ? [new Paragraph({
              text: `Phí dịch vụ: ${formatCurrency(contract.serviceFee)}`,
              spacing: { after: 200 },
            })] : []),
            ...(contract.paymentTerms ? [new Paragraph({
              text: `Điều kiện thanh toán: ${contract.paymentTerms}`,
              spacing: { after: 200 },
            })] : []),
          ] : []),
          ...(contract.contractType === 'Hợp đồng mua bán' ? [
            new Paragraph({
              text: 'Bên B bán cho Bên A:',
              spacing: { after: 200 },
            }),
            ...(contract.productDescription ? [new Paragraph({
              text: contract.productDescription,
              spacing: { after: 200 },
            })] : []),
            ...(contract.totalAmount ? [new Paragraph({
              text: `Tổng giá trị: ${formatCurrency(contract.totalAmount)}`,
              spacing: { after: 200 },
            })] : []),
            ...(contract.paymentTerms ? [new Paragraph({
              text: `Điều kiện thanh toán: ${contract.paymentTerms}`,
              spacing: { after: 200 },
            })] : []),
            ...(contract.deliveryTerms ? [new Paragraph({
              text: `Điều kiện giao hàng: ${contract.deliveryTerms}`,
              spacing: { after: 200 },
            })] : []),
          ] : []),

          new Paragraph({
            text: 'ĐIỀU 2: THỜI HẠN HỢP ĐỒNG',
            heading: HeadingLevel.HEADING_2,
            spacing: { after: 200 },
          }),
          new Paragraph({
            text: `Hợp đồng có hiệu lực từ ngày ${formatDate(contract.effectiveDate) || '__/__/____'}${contract.expiryDate ? ` đến ngày ${formatDate(contract.expiryDate)}` : ''}.`,
            spacing: { after: 400 },
          }),

          new Paragraph({
            text: 'ĐIỀU 3: QUYỀU VÀ NGHĨA VỤ',
            heading: HeadingLevel.HEADING_2,
            spacing: { after: 200 },
          }),
          ...renderTerms(),

          new Paragraph({
            text: 'ĐIỀU 4: ĐIỀU KHOẢN CHUNG',
            heading: HeadingLevel.HEADING_2,
            spacing: { after: 200 },
          }),
          new Paragraph({
            text: '1. Hợp đồng này được lập thành 02 (hai) bản, mỗi bên giữ 01 (một) bản có giá trị pháp lý như nhau.',
            spacing: { after: 200 },
          }),
          new Paragraph({
            text: '2. Mọi tranh chấp phát sinh từ hợp đồng này sẽ được giải quyết thông qua thương lượng. Nếu không thương lượng được, sẽ đưa ra Tòa án nhân dân có thẩm quyền giải quyết.',
            spacing: { after: 200 },
          }),
          new Paragraph({
            text: '3. Hợp đồng này có hiệu lực kể từ ngày ký.',
            spacing: { after: 600 },
          }),

          ...(contract.notes ? [
            new Paragraph({
              text: 'GHI CHÚ',
              heading: HeadingLevel.HEADING_2,
              spacing: { after: 200 },
            }),
            new Paragraph({
              text: contract.notes,
              spacing: { after: 600 },
            }),
          ] : []),

          // Signature
          new Paragraph({
            children: [
              new TextRun({
                text: 'BÊN A',
                bold: true,
              }),
            ],
            alignment: AlignmentType.LEFT,
            spacing: { after: 800 },
          }),
          new Paragraph({
            text: contract.partyA?.representative || '_________________',
            alignment: AlignmentType.LEFT,
            spacing: { after: 200 },
          }),
          new Paragraph({
            text: contract.partyA?.position || '',
            alignment: AlignmentType.LEFT,
            spacing: { after: 1200 },
          }),

          new Paragraph({
            children: [
              new TextRun({
                text: 'BÊN B',
                bold: true,
              }),
            ],
            alignment: AlignmentType.RIGHT,
            spacing: { after: 800 },
          }),
          new Paragraph({
            text: contract.partyB?.name || '_________________',
            alignment: AlignmentType.RIGHT,
            spacing: { after: 200 },
          }),
          new Paragraph({
            text: contract.partyB?.position || '',
            alignment: AlignmentType.RIGHT,
          }),
        ],
      },
    ],
  });

  const blob = await Packer.toBlob(doc);
  saveAs(blob, `${contract.contractNumber || 'HopDong'}.docx`);
};

